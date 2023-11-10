#include <netinet/in.h>
#include <linux/netfilter.h>
#include <libnetfilter_queue/libnetfilter_queue.h>

#include <sched.h>
#include <sys/un.h>
#include <unistd.h>
#include <limits.h>

#include <cstdio>
#include <cerrno>
#include <cstdlib>
#include <csignal>

#include <queue>
#include <mutex>
#include <unordered_map>


#ifdef BUFSIZ
#undef BUFSIZ
#endif
#define BUFSIZ PIPE_BUF

#define PKTSIZ (BUFSIZ - (sizeof(uint32_t) * 2 + sizeof(timeval)))

#define STATE_NONE	0x00
#define STATE_DROP	0x01
#define STATE_ACCEPT	0x02


struct pktlog_t
{
    uint32_t uid;
    uint32_t dev;
    timeval tm;
    uint8_t payload[PKTSIZ];
};

struct backlog_t
{
    pktlog_t* log;
    void (* cb)(uint32_t state, void* arg);
    void* arg;
};

struct backlog_ret_t
{
    uint32_t uid;
    uint32_t stat;
};


std::unordered_map<uint32_t, backlog_t> g_pending;
std::queue<backlog_t> g_backlog;
std::mutex g_sync;
volatile bool g_alive;


int nfcb(
	struct nfq_q_handle* qh,
	struct nfgenmsg*,
	struct nfq_data* nfa,
	void*)
{
    int r;
    volatile int wh = 0;
    auto* log = new pktlog_t();
    
    log->uid = ntohl(nfq_get_msg_packet_hdr(nfa)->packet_id);
    log->dev = nfq_get_indev(nfa);
    
    if (!nfq_get_timestamp(nfa, &log->tm))
	goto E_ACCEPT;
    if (nfq_get_payload(nfa, reinterpret_cast<uint8_t**>(&log->payload)) < 0)
	goto E_ACCEPT;
    
    {
	std::unique_lock<std::mutex> lock(g_sync);
	g_backlog.emplace(
		log,
		[](int state, void* arg)
		{
			*reinterpret_cast<volatile int*>(arg) = state;
		},
		&wh
	);
    }
    while (!wh)
	sched_yield();
    
    r = nfq_set_verdict(qh, log->uid, wh == STATE_DROP ? NF_DROP : NF_ACCEPT, 0, nullptr);
    return r;

E_ACCEPT:
    r = nfq_set_verdict(qh, log->uid, NF_ACCEPT, 0, nullptr);
    delete log;
    return r;
}

void* pcb(void*)
{
    constexpr const char* c_sock = "/tmp/fr_nf";
    
    int fd = socket(AF_UNIX, SOCK_DGRAM, 0);
    if (fd == -1)
    {
	perror("socket()");
	pthread_exit(nullptr);
	// no-return
    }
    
    sockaddr_un server;
    server.sun_family = AF_UNIX;
    strcpy(server.sun_path, c_sock);
    
    unlink(c_sock);
    if (bind(fd, (struct sockaddr*)&server, sizeof server) == -1)
    {
	perror("bind()");
	close(fd);
	pthread_exit(nullptr);
	// no-return
    }
    
    for (; g_alive; )
    {
	/* WRITE */
	bool emtpy;
	backlog_t bl;
	{
	    std::unique_lock<std::mutex> lock(g_sync);
	    emtpy = g_backlog.empty();
	    if (!emtpy)
	    {
		bl = g_backlog.front();
		g_backlog.pop();
		g_pending[bl.log->uid] = bl;
	    }
	}
	if (!emtpy && send(fd, bl.log, sizeof *bl.log, MSG_CONFIRM) < 0)
	{
	    perror("send()");
	    pthread_exit(nullptr);
	    // no-return
	}
	
	/* READ */
	backlog_ret_t ret;
	if (recv(fd, &ret, sizeof ret, MSG_DONTWAIT | MSG_WAITALL) < 0)
	{
	    if (errno == EAGAIN || errno == EWOULDBLOCK)
		continue;
	    perror("recv()");
	    pthread_exit(nullptr);
	    // no-return
	}
	
	/* CALL-BACK */
	bl = g_pending[ret.uid];
	bl.cb(ret.stat, bl.arg);
	
	/* FINALIZE */
	delete bl.log;
    }
}

void term(int)
{
    g_alive = false;
}

int main()
{
    signal(SIGINT, term);
    signal(SIGTERM, term);
    
    struct nfq_handle* h;
    struct nfq_q_handle* qh;
    pthread_t tpcb;
    
    int fd;
    size_t rv;
    
    char* buf = static_cast<char*>(aligned_alloc(8, PKTSIZ));
    
    puts("opening nfq handle");
    if (!(h = nfq_open()))
    {
	perror("nfq_open()");
	return 1;
    }
    
    puts("unbinding nfq handler");
    if (nfq_unbind_pf(h, AF_INET) < 0 ||
	nfq_unbind_pf(h, AF_INET6) < 0)
    {
	perror("nfq_unbind_pf()");
	return 1;
    }
    
    puts("binding nfq handler");
    if (nfq_bind_pf(h, AF_INET) < 0 ||
	nfq_bind_pf(h, AF_INET6) < 0)
    {
	perror("nfq_bind_pf()");
	return 1;
    }
    
    puts("binding socket to nfq '0'");
    if (!(qh = nfq_create_queue(h, 0, &nfcb, nullptr)))
    {
	perror("nfq_create queue()");
	return 1;
    }
    
    if (nfq_set_mode(qh, NFQNL_COPY_PACKET, 0xFFFF) < 0)
    {
	perror("nfq_set_mode()");
	return 1;
    }
    
    fd = nfq_fd(h);
    
    g_alive = true;
    pthread_create(&tpcb, nullptr, pcb, nullptr);
    for (; g_alive;)
    {
	if ((rv = recv(fd, buf, PKTSIZ, 0)) >= 0)
	{
	    nfq_handle_packet(h, buf, static_cast<int>(rv));
	    continue;
	}
	
	if (errno == ENOBUFS)
	    continue;
	
	perror("recv()");
	break;
    }
    pthread_join(tpcb, nullptr);
    
    puts("unbinding from nfq '0'");
    nfq_destroy_queue(qh);
    
    puts("closing nfq handle");
    nfq_close(h);
    
    free(buf);
    
    return 0;
}
